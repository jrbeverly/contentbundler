Simple project:

A console application that does the following:

(1) Point to directory, gets all files that are .xnb
(2) Gets a list of the files

Then produces the following output

(1) A super zipped up metaarchive (simlpe .zip)
(2) A big .cs file generated with the following properties

'using <>.AssetEntitity` (or something like this)

With a large set of constants like so

```
* <>Content
    * Models
        * Skybox : AssetEntity
        * Plane : AssetEntity
    * Textures
        * SkyboxTexture : AssetEntity
```

The objective is quick and easy file searching by application