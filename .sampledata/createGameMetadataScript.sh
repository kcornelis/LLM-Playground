#!/bin/bash
# Usage: chmod +x ./createGameMetadataScript.sh
#        tar -xzvf games.tar.gz
#        ./createGameMetadataScript.sh games_metadata.json ../GameCatalog/Playground.GameCatalog.Migrations/DataSeeding/02_game_metadata_inserts.sql

input_json="$1"
output_sql="$2"

echo "-- SQL update statements for Games
IF EXISTS (SELECT 1 FROM Games WHERE DESCRIPTION IS NOT NULL)
BEGIN
   RETURN;
END;

" > "$output_sql"

counter=0

while read -r item; do
    app_id=$(echo "$item" | jq '.app_id')
    description=$(echo "$item" | jq -r '.description' | tr -cd 'a-zA-Z0-9 ')
    tags=$(echo "$item" | jq -c '.tags' | sed "s/'/''/g")

    echo "UPDATE Games SET Description = N'$description', Tags = '$tags' WHERE Id = $app_id;" >> "$output_sql"

    counter=$((counter + 1))
    if ((counter % 100 == 0)); then
        printf "Processed %d records...\n" "$counter" > "/dev/stderr"
    fi
done < "$input_json"