#!/bin/bash

kill $(ps ax | grep '/Applications/Stream Deck.app/Contents/MacOS/Stream Deck' | awk '{print $1}' | head -1)