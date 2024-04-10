<script>
  import {
    UserIcon,
    ImageIcon,
    MoreHorizontalIcon,
  } from "svelte-feather-icons";
</script>

<svelte:head>
  <style>
    .shared-browser {
      display: flex;
      height: 90%;
    }
    .shared-section {
      display: flex;
      flex-direction: column;
      width: 70%;
      height: 100%;
    }
    .title {
      font-weight: bold;
      font-size: 3rem;
      color: #ffc107;
      margin-left: 50px;
    }
    .shared-folder {
      margin-left: 50px;
      display: flex;
      width: 85%;
      overflow: auto;
      height: 100%;
      align-items: center;
    }

    .folder {
      width: 200px;
      height: 150px;
      background-color: var(--primary);
      border-radius: 25px;
      margin: 10px;
      padding: 10px;
      color: white;
    }
    .folder-users {
      height: 50%;
      width: 100%;
      overflow: hidden;
      display: flex;
      align-items: center;
    }
    .user-icon {
      background-color: gray;
      height: 50px;
      border-radius: 50%;
      width: 50px;
      display: flex;
      justify-content: center;
      align-items: center;
      overflow: hidden;
      margin: 5px;
      flex-shrink: 0;
    }
    .folder-name {
      height: 25%;
      width: 100%;
      overflow: hidden;
      text-overflow: ellipsis;
      font-weight: bold;
    }
    .file-number {
      height: 25%;
      width: 100%;
    }
    .shared-files {
      width: 85%;
      height: 50%;
    }
    .files-title {
      font-weight: bold;
      font-size: 3rem;
      color: #ffc107;
      margin-left: 50px;
      margin-top: 0px;
      margin-bottom: 0px;
    }
    .shared-preview {
      background-color: white;
      width: 20%;
      height: 100%;
      border-radius: 30px;
      margin: 50px;
    }
    .shared-column {
      padding: 50px;
      padding-top: 0px;
      padding-bottom: 0px;
      width: 100%;
      height: 100%;
      overflow: auto;
      display: flex;
      justify-content: space-evenly;
      flex-wrap: wrap;
      align-content: flex-start;
    }
    .files {
      display: flex;
      flex-direction: column;
      height: 225px;
      border: solid;
      border-color: black;
      border-radius: 15px;
      background-color: white;
      font-size: 0.8rem;
      width: 175px;
      align-items: center;
      overflow: auto;
      margin-bottom: 20px;
    }
    .fileIcon {
      display: flex;
      margin: 2px;
      height: 50%;
      align-items: center;
      justify-content: center;
      width: 50%;
    }
    .fileName {
      margin: 2px;
      overflow: hidden;
      text-overflow: ellipsis;
      font-weight: bold;
      max-width: 90%;
      font-size: 0.9rem;
    }
    .fileType {
      margin: 2px;
      max-width: 90%;
    }
    .fileSize {
      margin: 2px;
      max-width: 90%;
    }
    .moreIcon {
      margin: 2px;
      align-self: flex-end;
    }
  </style>
</svelte:head>

<div class="shared-browser">
  <div class="shared-section">
    <div class="titleContainer">
      <p class="title">Shared Folder</p>
    </div>
    <div class="shared-folder">
      <div class="folder">
        <div class="folder-users">
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
        </div>
        <div class="folder-name"><p>Pictures</p></div>
        <div class="file-number"><p>380 files</p></div>
      </div>
      <div class="folder">
        <div class="folder-users">
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
        </div>
        <div class="folder-name"><p>Pictures</p></div>
        <div class="file-number"><p>380 files</p></div>
      </div>
      <div class="folder">
        <div class="folder-users">
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
        </div>
        <div class="folder-name"><p>Pictures</p></div>
        <div class="file-number"><p>380 files</p></div>
      </div>
      <div class="folder">
        <div class="folder-users">
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
        </div>
        <div class="folder-name"><p>Pictures</p></div>
        <div class="file-number"><p>380 files</p></div>
      </div>
      <div class="folder">
        <div class="folder-users">
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
          <div class="user-icon"><UserIcon size="35%" /></div>
        </div>
        <div class="folder-name"><p>Pictures</p></div>
        <div class="file-number"><p>380 files</p></div>
      </div>
    </div>
    <div class="shared-files">
      <div class="shared-files-title"><p class="files-title">Files</p></div>
      <div class="shared-column">
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
        <div class="files">
          <div class="fileIcon"><ImageIcon size="90%" /></div>
          <div class="fileName">fileNamePlaasfdfdfsdafsdaasdfceholder</div>
          <div class="fileType">fileTypePlaceholder</div>
          <div class="fileSize">fileSizePlaceholder</div>
          <div class="moreIcon"><MoreHorizontalIcon /></div>
        </div>
      </div>
    </div>
  </div>

  <div class="shared-preview">
    <p>bruh</p>
  </div>
</div>
