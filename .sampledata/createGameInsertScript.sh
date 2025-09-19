#!/bin/bash
# Usage: chmod +x ./createGameInsertScript.sh
#        tar -xzvf games.tar.gz
#        ./createGameInsertScript.sh games.csv ../GameCatalog/Playground.GameCatalog.Migrations/DataSeeding/01_game_inserts.sql

input_csv="$1"
output_sql="$2"

# Write header for SQL file
echo "-- SQL insert statements for Games
IF EXISTS (SELECT 1 FROM Games)
BEGIN
   RETURN;
END;

SET IDENTITY_INSERT Games ON;
" > "$output_sql"

counter=0

# Skip header and process each line
tail -n +2 "$input_csv" | while IFS=',' read -r Id Title ReleaseDate Windows Mac Linux Rating PositiveRatio Reviews Price OriginalPrice Discount SteamDeck
do
    # Escape single quotes and $ character in Title
    Title=$(echo "$Title" | tr -cd 'a-zA-Z0-9 ')
    
    # Format booleans (assume 1/0 or true/false in CSV)
    for var in Windows Mac Linux SteamDeck; do
        eval "$var=\$(echo \${$var} | tr -d '\r' | awk '{if(tolower(\$0) == \"true\") print 1; else if(tolower(\$0) == \"false\") print 0; else print \$0}')"
    done

    # Build SQL INSERT statement
    echo "INSERT INTO Games (Id, Title, ReleaseDate, Windows, Mac, Linux, SteamDeck, Rating, PositiveRatio, Reviews, Price, OriginalPrice, Discount) VALUES ('$Id', N'$Title', '$ReleaseDate', $Windows, $Mac, $Linux, $SteamDeck, '$Rating', $PositiveRatio, $Reviews, $Price, $OriginalPrice, $Discount);" >> "$output_sql"

    counter=$((counter + 1))
    if ((counter % 100 == 0)); then
        printf "Processed %d records...\n" "$counter" > "/dev/stderr"
    fi
done

echo "
SET IDENTITY_INSERT Games OFF;
" >> "$output_sql"