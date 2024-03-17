<script lang="ts">
  import { SearchIcon } from "svelte-feather-icons";
  import { ImageIcon} from "svelte-feather-icons";
  import { MoreVerticalIcon } from "svelte-feather-icons";
</script>

<style>
  .form{
    display:flex;
    justify-content: flex-start;
    align-items: flex-start;
    align-items: flex-start;
    padding-top: 50px;
    padding-left: 50px;
    padding-bottom: 50px;
  }
  .file-browser{
    margin-left:10%;
    background-color: #3C4F49;
  }
  .files-section{
    margin-top: 10px;
    display: grid;
    grid-template-columns: auto auto;
    padding-left: 50px;
    padding-bottom: 50px;
  }
  .title{
    font-weight: bold;
    font-size: 24px;
    color: #FFC107;
  }
  .files-column{
    width: 750px;
  }
  .files{
    margin-top: 20px;
    border: solid;
    border-color: black;
    border-radius: 15px;
    background-color: white;
    display: grid;
    grid-template-columns: auto auto auto auto auto auto auto auto auto auto auto;
  }
  .fileIcon{
    margin: 5px;
  }
  .fileName{
    margin: 5px;
    grid-column: 2 / 5;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .fileType{
    margin: 5px;
    grid-column-end: 5 / 7;
  }
  .fileSize{
    margin: 5px;
    grid-column: 7 / 9;
  }
  .moreIcon{
    margin: 5px;
    grid-column: 11 / 12;
  }
  .files-preview{
    background-color: whitesmoke;
    border: solid;
    border-color: black;
    grid-column: 2;
    margin-right: 10%;
    max-height:fit-content;
    max-width:400px;
    display: grid;
    grid-template-columns: auto;
    justify-content: center;
    border-radius: 30px;
  }
  .previewIcon{
    margin-top: 10%;
    width: 200px;
    height: 200px;
  }
  .previewFileName{
    margin-top: 7px;
    font-size: 24px;
    font-weight: bold;
    max-width: 200px;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .previewfileType{
    margin-top: 7px;
  }
  .previewFileSize{
    margin-top: 7px;
  }
  .previewFileDate{
    margin-top: 7px;
  }

</style>

<div class="file-browser">
  <div class="form">
    <form class="search">
        <label for="default-search" class="mb-2 text-sm font-medium text-gray-900 sr-only dark:text-white">Search</label>
        <div class="relative">
            <div class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <svg class="w-4 h-4 text-gray-500 dark:text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
                </svg>
            </div>
            <div class="w-[750px]">
              <input type="search" id="default-search"class="block w-full p-4 pl-10 text-sm text-gray-100 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-300 dark:border-gray-200 dark:placeholder-gray-100 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500 w-full " placeholder="Search your files" required>
              <button type="submit" class="text-white absolute right-2.5 bottom-2.5 bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Search</button>
            </div>
        </div>
    </form>
  </div>

    <div class="files-section">
      <div class="files-column">
        <p class="title">Placeholder</p>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon/></div>
          <div class="fileName">fileNamePlaceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreVerticalIcon/></div>
        </div>
      </div>

      <div class="files-preview">
        <div class="files-preview-info">
          <div class="previewIcon"><img src="https://cdn4.iconfinder.com/data/icons/ionicons/512/icon-image-512.png"></div>
          <div class="previewFileName">fileNamePlaceholder</div>
          <div class="previewfileType">fileTypePlaceholder</div>
          <div class="previewFileSize">fileSizePlaceholder</div>
          <div class="previewFileDate">00/00/0000</div>
        </div>

      </div>

    </div>

</div>
