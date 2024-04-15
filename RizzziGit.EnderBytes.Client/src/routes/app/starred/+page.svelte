<script>

    import { ImageIcon, MoreHorizontalIcon, Trash2Icon, EditIcon, Share2Icon } from "svelte-feather-icons";

</script>

<svelte:head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width">
    <title>Starred</title>
    <style>
        body{
          margin:0px;
        }
        .starred-section{
          display: grid;
          grid-template-columns: auto auto;
          margin: 5%;
          margin-left: 50px;
          width: 85vw;

        }
        .title{
          font-weight: bold;
          font-size: clamp(2rem, 3rem, 4rem);
          color: #FFC107;
          margin-left: 50px;
        }
        .starred-column{
          width: 100%;
          height: 70vh;
          overflow: auto;
          display: flex;
          justify-content: space-evenly;
          flex-wrap: wrap;
          align-content: space-between;
        }
        .files{
          display: flex;
          flex-direction: column;
          height: 300px;
          border: solid;
          border-color: black;
          border-radius: 15px;
          background-color: white;
          font-size: 25px;
          width: 250px;
          align-items: center;
          overflow: auto;
          margin-bottom: 20px;
        }
        .fileIcon{
          display: flex;
          margin: 2px;
          height: 50%;
          align-items: center;
          justify-content: center;
          width: 50%;
        }
        .fileName{
          margin: 2px;
          overflow: hidden;
          text-overflow: ellipsis;
          font-weight:bold;
          max-width: 90%;
        }
        .fileType{
          margin: 2px;
          max-width: 90%;
        }
        .fileSize{
          margin: 2px;
          max-width: 90%;
        }
        .moreIcon{
          margin: 2px;
          align-self: flex-end;
        }
        .my-icon1{
          font-size: 40px;
          margin: auto;
          width: 50%;
          padding-right; 50px;
        }
        .my-icon2{

        }
        .my-icon3{

        }
        .starred-preview{
            background-color: whitesmoke;
            border: solid;
            border-color: black;
            grid-column: 2;
            max-height: fit-content;
            width: 70%;
            display: flex;
            justify-content: flex-start;
            border-radius: 30px;
            flex-direction: column;
            opacity: 0;
            transition: opacity .5s;
            justify-self: center;

        }
        .starred-preview-info{
            display: flex;
            flex-direction: column;
            align-items: center;
        }
        .previewIcon{
            display: flex;
            justify-content: center;
        }
        .previewFileName{
          margin-top: 7px;
          font-size: 2rem;
          font-weight: bold;
          max-width: 100%;
          overflow: hidden;
          text-overflow: ellipsis;
        }
        .previewfileType{
          margin-top: 7px;
          font-size: 1rem;
        }
        .previewFileSize{
          margin-top: 7px;
          font-size:1rem;
        }
        .previewFileDate{
          margin-top: 7px;
          font-size: 1rem;
        }
        .starred-column:hover + .starred-preview{
            opacity: 1;
        }

        .fileIconImg,.moreIconImg{
          width: 30px;
        }

        @media only screen and (max-width: 1440px){
          .starred-section{
            margin: 0px;
          }
          .files{
            width:175px;
            height: 225px;
            font-size: .8rem;
          }
          .fileName{
            font-size: .9rem;
          }

        }

        @media only screen and (max-width: 1024px){
          .starred-section{
            width: 75vw;
            display: flex;
            height: 70vh
          }
          .starred-column{
            height: 100%;
            width: 70%;

          }
          .files{
            width: 150px;
            height: 200px;
          }
          .starred-preview{
            width: 20%;
            height: 70%;
            font-size: .8rem;
          }
          .previewFileName{
            font-size: .9rem;
          }
          .previewfileType,.previewFileSize,.previewFileDate{
          font-size: .8rem;
        }

        }
        @media only screen and (max-width: 768px){
          .starred-browser{
            height: auto;
          }
          .starred-section{
            height: 50vh;
            width: 100vw;
            margin: 0px;
            padding: 0px;
            justify-content: center;
          }
          .starred-column{
            width: 90vw;
            overflow: unset;
          }
          .title{
            margin: 0px;
          }
          .searchBarTitle{
            padding: 25px;
            padding-left: 40px;
            background-color: var(--background);
            position: sticky;
            top: 0;
          }
          .starred-preview{
            display: none;
          }



        }
        @media only screen and (max-width: 425px) {
          body,html{
            width: 100%;
            overflow-x: hidden;
          }
          .starred-browser{
            margin: 0px;
            width: 100%;
            border: 0px;
          }
          .searchBarTitle{
            display: flex;
            flex-direction: column;
            justify-content: center;
            width: 100%;
            padding: 10px;
            background-color: rgba(0,0,0,0);
            position: relative;
          }

          .starred-preview{
            display: none;
          }
          .starred-section {
            width:100%;
            margin: 0px;
            justify-content: center;
          }
          .title{
            margin-top:20px;
            margin-bottom: 20px;
            margin-left: 0px;
            font-size: 2rem;
          }
          .starred-column{
            overflow: unset;
            width: 100%;
            height: 100%;
          }
          .files{
            font-size: .75rem;
          }
          .fileName{
            font-weight: bold;
            font-size: .8rem;
          }

          .fileIcon, .moreIcon{
          }


        }
      </style>
</svelte:head>
<body>
    <div class="starred-browser">
        <div class="searchBarTitle">
          <div class="titleContainer">
            <p class="title">Starred Files</p>
          </div>
        </div>
          <div class="starred-section">
            <div class="starred-column">
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
              <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
                <div class="files">
                <div class="fileIcon"><ImageIcon size="90%"/></div>
                <div class="fileName">fileNamePlaceholder</div>
                <div class="fileType">fileTypePlaceholder</div>
                <div class="fileSize">fileSizePlaceholder</div>
                <div class="moreIcon"><MoreHorizontalIcon/></div>
              </div>
            </div>

            <div class="starred-preview">
                <div class="starred-preview-icon">
                    <div class="previewIcon"><img alt="preview" src="https://cdn4.iconfinder.com/data/icons/ionicons/512/icon-image-512.png" width="80%"></div>
                </div>
              <div class="starred-preview-info">
                <div class="previewFileName">fileNamePlaceholder</div>
                <div class="previewfileType">fileTypePlaceholder</div>
                <div class="previewFileSize">fileSizePlaceholder</div>
                <div class="previewFileDate">00/00/0000</div>
                <div class="my-icon1"><Trash2Icon/></div>
                <div class="my-icon2"><Share2Icon/></div>
                <div class="my-icon3"><EditIcon/></div>
              </div>

            </div>

          </div>

      </div>
</body>
