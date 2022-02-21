using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character
{

    public string CharacterName;
    public Texture2D Image;

    public Character(string Name, Texture2D texture)
	{
        CharacterName = Name;
        Image = texture;
	}
}
