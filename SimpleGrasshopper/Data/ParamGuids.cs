﻿namespace SimpleGrasshopper.Data;

/// <summary>
/// The built-in guids for some of the <see cref="IGH_Param"/>.
/// </summary>
public readonly struct ParamGuids
{
    /// <summary>
    /// 
    /// </summary>
    public const string
        AngularDimension = "1eb74534-dba3-4d5b-b177-264c9dd58549",
        Centermark = "71dfdc22-9723-4c6a-a541-a7b25c66b1a2",
        Hatch = "a4bfb03e-700d-4517-8c6b-1e88b466a781",
        InstanceReference = "6e669311-7c38-400b-94bd-cc3684c71204",
        Leader = "445f328a-45b3-4c67-a784-dc354a8992cd",
        LinearDimension = "ba477838-8571-4bfe-8e3c-cd71bd946d7d",
        OrdinateDimension = "b23c41cf-6b36-46f1-9843-bcc0d9d69301",
        RadialDimension = "9b1495a1-6c40-447a-b8fb-60b7ea71b47f",
        TextDot = "2773c606-0a71-46d2-9921-e63e22e09e3b",
        TextEntity = "79a2eb73-658a-41a1-99e6-c12fe3357de0",
        MeshParameters = "c3407fda-b505-4686-9165-38fe7a9274cf",
        Interval2D = "90744326-eb53-4a0e-b7ef-4b45f5473d6e",
        Vector = "16ef3e75-e315-4899-b531-d3166b42dac9",
        Arc = "04d3eace-deaa-475e-9e69-8f804d687998",
        Boolean = "cb95db89-6165-43b6-9c41-5702bc5bf137",
        Box = "c9482db6-bea9-448d-98ff-fed6d69a8efc",
        Brep = "919e146f-30ae-4aae-be34-4d72f555e7da",
        Circle = "d1028c72-ff86-4057-9eb0-36c687a4d98c",
        Colour = "203a91c3-287a-43b6-a9c5-ebb96240a650",
        Complex = "476c0cf8-bc3c-4f1c-a61a-6e91e1f8b91e",
        Culture = "7fa15783-70da-485c-98c0-a099e6988c3e",
        Curve = "d5967b9f-e8ee-436b-a8ad-29fdcecf32d5",
        Extrusion = "f638f034-5ce2-4536-8fa5-2c6ab8e9456c",
        Field = "3175e3eb-1ae0-4d0b-9395-53fd3e8f8a28",
        FilePath = "06953bda-1d37-4d58-9b38-4b3c74e54c8f",
        Geometry = "ac2bc2cb-70fb-4dd5-9c78-7e1ea97fe278",
        Group = "b0851fc0-ab55-47d8-bdda-cc6306a40176",
        Guid = "faf6e3bb-4c84-4cbf-bd88-6d6a0db5667a",
        Integer = "2e3ab970-8545-46bb-836c-1c11e5610bce",
        Interval = "15b7afe5-d0d0-43e1-b894-34fcfe3be384",
        LatLonLocation = "87391af3-35fe-4a40-b001-2bd4547ccd45",
        Line = "8529dbdf-9b6f-42e9-8e1f-c7a2bde56a70",
        Matrix = "bd4a8a18-a3cc-40ba-965b-3be91fee563b",
        Mesh = "1e936df3-0eea-4246-8549-514cb8862b7a",
        MeshFace = "e02b3da5-543a-46ac-a867-0ba6b0a524de",
        Number = "3e8ca6be-fda8-4aaf-b5c0-3c54c8bb7312",
        GenericObject = "8ec86459-bf01-4409-baee-174d0d2b13d0",
        OGLShader = "288cfe66-f3dc-4c9a-bb96-ef81f47fe724",
        Plane = "4f8984c4-7c7a-4d69-b0a2-183cbb330d20",
        Point = "fbac3e32-f100-4292-8692-77240a42fd1a",
        PointCloud = "850b6368-ff26-48ce-9773-ac554ffbaeef",
        Predicate = "fb3e1397-9f12-4f57-89bb-3c12a57b6d70",
        Rectangle = "abf9c670-5462-4cd8-acb3-f1ab0256dbf3",
        ScriptVariable = "84fa917c-1ed8-4db3-8be1-7bdc4a6495a2",
        StructurePath = "56c9c942-791f-4eeb-a4f0-82b93f1c0909",
        SubD = "89cd1a12-0007-4581-99ba-66578665e610",
        Surface = "deaf8653-5528-4286-807c-3de8b8dad781",
        String = "3ede854e-c753-40eb-84cb-b48008f14fd4",
        Time = "81dfff08-0c83-4f1b-a358-14791d642d9e",
        Transform = "28f40e48-e739-4211-91bd-f4aefa5965f8",
        MatchText = "d0abb9f4-6c6b-47d3-993b-58507ed13071",
        ModelAttributeKey = "17223436-22ea-4192-a683-0f97f9014d8c",
        ModelContent = "39304976-7d5b-4dbc-b913-147bc3340e32",
        ModelFont = "91cba8ac-25ed-4ce5-9c14-59e806e573eb",
        ModelMeshingParameters = "c5b9e91e-e0a9-4789-89ca-aa5d3e410580",
        ModelUnitSystem = "92bc91e8-b235-4eed-86d5-0aced5c9c4bf",
        AnnotationArrow = "a6035ecc-5551-4ea2-ac00-56ec5c6b0dcf",
        AnnotationArrowSettings = "1a3cc1fa-9f8b-4537-b0ac-74f1688cd6fd",
        AnnotationDimensionSettings = "5de23a52-b490-4d85-9c87-fbc0e353ecaf",
        AnnotationLeaderSettings = "9f6e7314-7a4b-4b93-ad0d-54e1375ec24a",
        AnnotationTextSettings = "608e1d69-2b19-4a59-a752-871686ec29b4",
        AnnotationToleranceSettings = "345fd8f8-5fb0-4907-99fd-f21f3963e9b3",
        AnnotationUnitsSettings = "d16a6a70-bfb7-41fd-90c6-62734d1dd016",
        DateTimeFormat = "92fb7644-3d25-4ba1-ab06-469bd58add90",
        ModelAnnotationStyle = "5ca0914d-b372-4a5e-aef3-137bd34abb6f",
        ModelDisplayMode = "64539bd9-e33e-4cff-91dc-4987e8040d48",
        ModelPageViewport = "4e007c9e-6521-4819-b16f-bdd3e768e003",
        ModelStandardViewport = "2aab104a-5b20-4d8f-a918-fda5f016c828",
        ModelView = "7069208c-c471-4b82-bae6-e938f16dacb0",
        ObjectDisplay = "ce3d0672-fbed-4332-84b9-6f895dad48fa",
        ObjectDisplayColor = "3653cf51-4fa3-473b-b742-746c59a33796",
        ObjectDisplayMode = "8aa197e1-20fc-4662-8c05-1474668d3636",
        ObjectVisibility = "7132aa5e-db44-4ad4-afbc-f5d9e424d254",
        DisplayColorGradient = "ffa1d4f8-1dd2-4495-8f2f-2e741aba7c54",
        DisplayColorStop = "ad13f223-bb9e-48d8-a5ab-51b2557bd69e",
        ModelHatchLine = "778e8a8d-3ae6-4253-bd63-f0e725cb852e",
        ModelHatchPattern = "347f62f4-3c6e-4156-a92e-49cb5ade89dc",
        ModelLinetype = "c2c85a16-98f4-4f29-b1e4-9ea45744f80e",
        ModelLineWidth = "fc8387c3-cc00-4d2d-b3f8-4f2bfa894d6d",
        ObjectDrafting = "ae26c08c-3ca4-4fd5-af73-497dbcd626a3",
        ObjectDraftingColor = "3e972aa1-906f-4957-9e1f-956cb1429030",
        ObjectDraftingLinetype = "8ff0c4b8-0798-49c0-8f2e-e726edfe1b48",
        ObjectDraftingLineWidth = "22149be8-8a95-4018-af41-050d03d5709b",
        ModelInstanceDefinition = "988fbdfd-d3eb-4ead-ab83-d0db9f2a36d9",
        ModelLayer = "1c0601b4-7b16-4712-b764-18751be7298a",
        ModelObject = "1c30cbca-3907-4e28-9003-b06352f5459c",
        ModelRenderMaterial = "0b0088b7-9572-4e07-8f79-bff2dee3978a",
        ObjectRender = "3abbde21-9106-4846-882a-d1f8ca791cc0",
        ObjectRenderMaterial = "c17f7249-7f62-477d-b66d-fb79ef67eae0";
}
