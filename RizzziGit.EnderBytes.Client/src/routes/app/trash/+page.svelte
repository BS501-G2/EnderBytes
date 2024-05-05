<script>
	import { ImageIcon, MoreHorizontalIcon } from 'svelte-feather-icons';
</script>

<svelte:head>
	<meta charset="UTF-8" />
	<meta name="viewport" content="width=device-width" />
	<title>Recycle Bin</title>
	<style>
		body{
		background-color: --onPrimary;
		margin:0px;23
		}
		.recycle-browser{
		border-radius: 30px;
		}
		.recycle-section{
		display: grid;
		grid-template-columns: 70% 30%;
		margin: 5%;
		margin-left: 50px;
		}
		.title{
		font-weight: bold;
		font-size: clamp(2rem, 3rem, 4rem);
		color: #FFC107;
		margin-left: 50px;
		}
		.recycle-column{
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
		}
		.fileSize{
		margin: 2px;
		}
		.moreIcon{
		margin: 2px;
		align-self: flex-end;
		}
		.recycle-preview{
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
		.recycle-preview-info{
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
		font-size: clamp(1rem, 2rem, 2.5rem);
		font-weight: bold;
		max-width: 200px;
		overflow: hidden;
		text-overflow: ellipsis;
		}
		.previewfileType{
		margin-top: 7px;
		font-size: clamp(.5rem, 1rem, 1.5rem);
		}
		.previewFileSize{
		margin-top: 7px;
		font-size: clamp(.5rem, 1rem, 1.5rem);
		}
		.previewFileDate{
		margin-top: 7px;
		font-size: clamp(.5rem, 1rem, 1.5rem);
		}
		.recycle-column:hover + .recycle-preview{
			opacity: 1;
		}
		.searchBarTitle{
		width: clamp(500px, 1000px, 1500px);
		}
		.fileIconImg,.moreIconImg{
		width: 30px;
		}

		@media only screen and (max-width: 1024){
		body,html{
			width: 100%;
			overflow-x: hidden;
		}
		.recycle-section{
			height: 100vh;
			width: 100vw;
			margin: 0px;
			padding: 0px;
			justify-content: center;
		}
		.recycle-column{
			height: 100%;
			width: 90vw;
			overflow: unset;
		}
		}
		@media only screen and (max-width: 768px){
		body,html{
			width: 100%;
			overflow-x: hidden;
		}
		.recycle-section{
			height: 100vh;
			width: 100vw;
			margin: 0px;
			padding: 0px;
			justify-content: center;
		}
		.recycle-column{
			height: 100%;
			width: 90vw;
			overflow: unset;
		}
		.recycle-preview{
			display: none;
		}
		.recycle-browser{
			width: 100vw;
		}
		.searchBarTitle{
			width: 100vw;
		}
		.title{
			margin: 25px;
			margin-left: 40px;
		}
		.files{
			font-size: larger;
			width: 100%;
		}

		.searchBarTitle{
			display: flex;
			flex-direction: column;
			justify-content: center;
			padding-top: 10px;
		}

		}
		@media only screen and (max-width: 425px) {
		body,html{
			width: 100%;
			overflow-x: hidden;
		}
		.recycle-browser{
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
		}
		body{
			background-color: rgb(32, 32, 32);
		}
		.recycle-preview{
			display: none;
		}
		.recycle-section {
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
		.recycle-column{
			overflow: unset;
			width: 100%;
			height: 100%;
		}
		.files{
			background-color: rgb(39, 39, 39);
			border-left: 0px;
			border-right: 0px;
			border-top: 0px;
			color:white;
			border-color: lightgray;
			border-radius: 0px;
			font-size: .5rem;
			width: 100%;
			grid-template-columns: 75px 250px 75px;
			grid-template-rows: 25px 25px 25px;
			height: 75px;
			justify-items: center;
			margin-bottom:5px;
			resize: none;
		}
		.fileIcon{
			grid-row-start: 1;
			grid-row-end: 4;
		}
		.moreIcon{
			grid-column: 3;
			grid-row-start: 1;
			grid-row-end: 4;
		}
		.fileName{
			font-weight: bold;
			font-size: .75rem;
		}
		.fileName,.fileSize,.fileType{
			grid-column: 2;
			justify-self: start;
		}
		.fileIcon, .moreIcon{
			filter: invert(100%);
		}

		.fileIcon,.fileName,.fileType,.fileSize,.moreIcon{
			margin:0px;
		}
		.fileIconImg{
			width: 75px;
		}
		.moreIconImg{
			width: 25px;
		}
		}
	</style>
</svelte:head>

<div class="recycle-browser">
	<div class="searchBarTitle">
		<div class="titleContainer">
			<p class="title">Recycle Files</p>
		</div>
	</div>
	<div class="recycle-section">
		<div class="recycle-column">
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
			<div class="files">
				<div class="fileIcon"><ImageIcon size="90%" /></div>
				<div class="fileName">fileNamePlaceholder</div>
				<div class="fileType">fileTypePlaceholder</div>
				<div class="fileSize">fileSizePlaceholder</div>
				<div class="moreIcon"><MoreHorizontalIcon /></div>
			</div>
		</div>

		<div class="recycle-preview">
			<div class="recycle-preview-icon">
				<div class="previewIcon">
					<img
						src="https://cdn4.iconfinder.com/data/icons/ionicons/512/icon-image-512.png"
						width="80%"
						alt="Test"
					/>
				</div>
			</div>
			<div class="recycle-preview-info">
				<div class="previewFileName">fileNamePlaceholder</div>
				<div class="previewfileType">fileTypePlaceholder</div>
				<div class="previewFileSize">fileSizePlaceholder</div>
				<div class="previewFileDate">00/00/0000</div>
			</div>
		</div>
	</div>
</div>
