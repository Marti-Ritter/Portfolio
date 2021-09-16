library(tidyverse)
library(magrittr)
library(tximport)
library(data.table)
library(DESeq2)
library(dplyr)
library(biomaRt)
library(readxl)
library(ggplot2)
library(gridExtra)
library(EDASeq)

salmonfiles = Sys.glob("*/quant.genes.sf")

txi <- tximport(Sys.glob("*/quant.genes.sf"), type = "none", txOut = TRUE, txIdCol = "Name", abundanceCol = "TPM", 
                countsCol = "NumReads", lengthCol = "Length", importer = function(x) read_tsv(x, skip = 0))

dds <- DESeq2::DESeqDataSetFromTximport(txi,colData=data.frame(sample=basename(dirname(salmonfiles))),design= ~ sample)

dds@assays@data$counts%>%as.data.frame%>%set_colnames(colData(dds)$sample)%>%rownames_to_column('transcript_id')%>%write_tsv('salmon_counts.tsv')


countdata <- fread('salmon_counts.tsv')

mart<- useDataset("mmusculus_gene_ensembl", useMart("ensembl"))

genes <- countdata$transcript_id

gene_ids <- getBM(filters="ensembl_gene_id", attributes= c("ensembl_gene_id","external_gene_name", "start_position", "end_position"), values=genes, mart=mart)

countdata <- left_join(countdata, gene_ids, by = c("transcript_id"="ensembl_gene_id"))

countdata$length = countdata$end_position - countdata$start_position

countdata = subset(countdata, select = -c(start_position,end_position))

countdata%>%dplyr::select(external_gene_name, length, transcript_id, everything())%>%write_tsv('salmon_counts_named.tsv')

countdata <- fread('salmon_counts_named.tsv')
names(countdata)[names(countdata) == 'transcript_id'] <- 'ENSEMBLID'

countdata <- distinct(countdata, ENSEMBLID, .keep_all = TRUE)
countdata <- countdata[complete.cases(ENSEMBLID),]

coldata<-countdata%>%colnames%>%tail(-3)%>%str_split('_')%>%map_df(~as.data.frame(t(.)))%>%
  set_colnames(c('celltype','treatment','replicate'))%>%set_rownames(colnames(countdata[,-c(1,2,3)]))

coldata%<>%map_df(forcats::as_factor)

new_countdata <- subset(countdata, select = -c(external_gene_name, length))
dds <- DESeqDataSetFromMatrix(new_countdata, colData = coldata,tidy=T,design = ~celltype + treatment + celltype:treatment)

rownames(dds) <- countdata$ENSEMBLID

dds <- DESeq(dds)

resultsNames(dds)

# res <- results(dds, contrast=c(0,1,0,0)) # Glut <-> GABA (noGLia)
# location <- 'Glut_vs_GABA(noGlia).csv'
# res <- results(dds, contrast=c(0,1,0,1)) # Glut <-> GABA (withGlia)
# location <- 'Glut_vs_GABA(withGlia).csv'
# res <- results(dds, contrast=c(0,0,1,0)) # GABA withGlia <-> noGlia
# location <- 'GABA_withGlia_vs_noGlia.csv'
res <- results(dds, contrast=c(0,0,1,1)) # Glut withGlia <-> noGlia
location <- 'Glut_withGlia_vs_noGlia.csv'

res$gene_name <- countdata$external_gene_name
res$length <- countdata$length

gene_names <- read_excel('gene_names.xlsx')
gene_names <- gene_names %>% column_to_rownames(., var = "Gene symbol")
gene_names <- gene_names[!(names(gene_names) %in% c('Gene symbol', 'Description'))]
most_abundant <- colnames(gene_names)[apply(gene_names,1,which.max)]

score_calculator <- function(input_row, output) {
  fold_change_max_to_second_largest <- max(input_row) /
    sort(input_row,partial=length(input_row)-1)[length(input_row)-1]
  
  proportion_max <- max(input_row) / sum(input_row)
  
  anti_neuron <- max(input_row) / input_row['Neuron']
  
  score <- fold_change_max_to_second_largest * proportion_max * anti_neuron
    
  return(score)
}

scores <- apply(gene_names, 1, score_calculator)

gene_names$most_abundant <- most_abundant
gene_names$score <- scores
gene_names$gene_name <- rownames(gene_names)
gene_names%>%dplyr::select(c('gene_name', 'most_abundant', 'score'), everything())%>%write_excel_csv2('gene_names_processed.csv')

gene_names_processed <- read_csv2('gene_names_processed.csv')
res_df <- data.frame(res)
res_df$gene_id <- rownames(res_df)
res_df <- res_df[(res_df$gene_name %in% gene_names_processed$gene_name),]
gene_names_processed <- left_join(data.frame(res_df), gene_names_processed, by = c("gene_name"="gene_name"))

gene_names_with_counts <- left_join(countdata[(countdata$external_gene_name %in% gene_names_processed$gene_name),3:15], gene_names_processed, by = c("ENSEMBLID"="gene_id"))

gene_names_sorted <- gene_names_with_counts[order(gene_names_with_counts$most_abundant, -gene_names_with_counts$score),]
gene_names_sorted%>%write_excel_csv(paste('combined_tables\\', location, sep=''))

reduced_table <- gene_names_sorted[,c('gene_name', 'gene_id', 'most_abundant', 'score', 'baseMean', 'log2FoldChange', 'padj', 'length')] 
reduced_table%>%write_excel_csv(paste('combined_tables\\', location, sep=''))

gene_names_full <- read.csv2('gene_names_full.csv')
celltypes <- c('Astrocytes', 'Microglia', 'Myelinating Oligodendrocytes', 'Newly Formed Oligodendrocyte', 'Oligodendrocyte Precursor Cell', 'Endothelial Cells')

plot_list = c()

create_histograms <- function (celltype, data) {
  relevant_df <- top_n(data[data$most_abundant == celltype,],100,score)
  significant_df <- relevant_df[relevant_df$significant == TRUE,]
  relevant_df$origin <- 'full'
  significant_df$origin <- 'significant'
  plot_df <- rbind(relevant_df, significant_df)
  ggplot(plot_df,aes(x=log2FoldChange, group=origin, fill=origin)) + 
    geom_histogram(position="identity",alpha=0.5,binwidth=0.25) + theme_bw()
}

resOrdered <- res[order(res$pvalue),]

summary(res)

sum(res$padj < 0.1, na.rm=TRUE)

res05 <- results(dds, alpha=0.05)
sum(res05$padj < 0.05, na.rm=TRUE)

plotMA(res, ylim=c(-2,2))

plotCounts(dds, gene=which.min(res$padj), intgroup="celltype")


# from PiGx Pipeline shared by colleague

# table most padj genes
res <- res[order(res$padj),]
DE <- as.data.frame(res)

DEsubset <- DE[!is.na(DE$padj) & abs(DE$log2FoldChange) > 1,]
max <- 1000
if(nrow(DEsubset) < max) {
  max <- nrow(DEsubset)
}
DEsubset <- DEsubset[1:max,]

DT::datatable(DEsubset, 
              extensions = c('Buttons', 'FixedColumns', 'Scroller'), 
              options = list(fixedColumns = TRUE, 
                             scrollY = 400,
                             scrollX = TRUE,
                             scroller = TRUE,
                             dom = 'Bfrtip',
                             buttons = c('colvis', 'copy', 'print', 'csv','excel', 'pdf'),
                             columnDefs = list(
                               list(targets = c(3,4,5), visible = FALSE)
                             )),
              filter = 'bottom'
)

# quantiles
countData <- countdata %>%remove_rownames() %>%column_to_rownames(var = 'external_gene_name')
readCounts <- as.data.frame(colSums(countData))
readCounts$group <- coldata[rownames(readCounts),]$treatment
readCounts$sample <- rownames(readCounts)
colnames(readCounts)[1] <- 'readCounts'

quantiles <- quantile(readCounts$readCounts, c(1:20)/20)[c(1,5,15,19)]

library(ggrepel)

p <- ggplot(readCounts, aes(x = sample, y = readCounts)) + geom_bar(aes(fill = group), stat = 'identity') + 
  geom_hline(yintercept = as.numeric(quantiles), color = 'red') +
  geom_label_repel(data = data.frame(x = 0, y = as.numeric(quantiles)), aes(x = x, y = y, label = names(quantiles))) + theme(legend.position = 'bottom') + scale_y_continuous(labels = scales::comma) +  coord_flip()
print(p)
imagesDir <- './images'
pdf(file = file.path(imagesDir, 'readcounts.pdf'))
print(p)
invisible(dev.off())

#p_value histogram
p <- ggplot(data = DE, aes(x = pvalue)) + geom_histogram(bins = 100)
print(p)

#save image to folder
pdf(file = file.path(imagesDir, 'pvalue_histogram.pdf'))
print(p)
invisible(dev.off())

# MA plot
DESeq2::plotMA(res, main=paste("MA plot"))

#save image to folder
pdf(file = file.path(imagesDir, 'MA_plot.pdf'))
DESeq2::plotMA(res, main=paste("MA plot"))
invisible(dev.off())

vsd <- DESeq2::varianceStabilizingTransformation(dds)
vsd.counts = SummarizedExperiment::assay(vsd)

#PCA
plotGroups <- c('celltype', 'treatment')
pcaPlots <- lapply(plotGroups, function(g) {
  pca <- stats::prcomp(t(vsd.counts), center = TRUE)
  pcaSummary <- summary(pca)
  df <- merge(as.data.frame(pca$x), coldata, by = 'row.names')
  ggplot(df, aes(x = PC1, y = PC2)) + 
    geom_point(aes_string(color = g)) + 
    geom_label_repel(aes(label = Row.names), size = 3) + 
    labs(x = paste0('PC1 (',round(pcaSummary$importance[2, 'PC1'] * 100, 1),'%)'), 
         y = paste0('PC2 (',round(pcaSummary$importance[2, 'PC2'] * 100, 1),'%)')) + 
    theme_bw()
})
#save image to folder
pdf(file = file.path(imagesDir, 'pcaPlots.pdf'))
for(p in pcaPlots){
  print(p)  
}
invisible(dev.off())

# Correlation
M <- stats::cor(vsd.counts)
corrplot::corrplot(corr = M, order = 'hclust', method = 'square', type = 'lower', tl.srt = 45, addCoef.col = 'white')

#save image to folder
pdf(file = file.path(imagesDir, 'correlationPlot.pdf'))
corrplot::corrplot(corr = M, order = 'hclust', method = 'square', type = 'lower', tl.srt = 45, addCoef.col = 'white')
invisible(dev.off())

#heatmap
select <- na.omit(names(sort(apply(X = vsd.counts, MARGIN = 1, FUN = var),decreasing = T))[1:100])
df <- as.data.frame(coldata[,c("treatment","celltype")])
pheatmap::pheatmap(vsd.counts[select,], 
                   cluster_rows=TRUE, 
                   show_rownames=FALSE, 
                   cluster_cols=TRUE, 
                   annotation_col=df, 
                   main = 'Heatmap of the Normalized Expression Values (VST) of \n Top 100 Genes with highest variance across samples')

#save image to folder
pdf(file = file.path(imagesDir, 'heatmap.pdf'))
pheatmap::pheatmap(vsd.counts[select,], 
                   cluster_rows=TRUE, 
                   show_rownames=FALSE, 
                   cluster_cols=TRUE, 
                   annotation_col=df, 
                   main = 'Heatmap of the Normalized Expression Values (VST) of \n Top 100 Genes with highest variance across samples')
invisible(dev.off())

# volcano
p <- ggplot(DE, aes(x = log2FoldChange, y = -log10(pvalue))) + geom_point(aes(color = padj < 0.1))
print(p)

#save image to folder
pdf(file = file.path(imagesDir, 'volcanoPlot.pdf'))
print(p)  
invisible(dev.off())

# summary
filterUP <- function(df, log2fc = 1, p = 0.1) {nrow(df[df$log2FoldChange >= log2fc & !is.na(df$padj) & df$padj <= p,])}
filterDOWN <- function(df, log2fc = 1, p = 0.1) {nrow(df[df$log2FoldChange < -log2fc & !is.na(df$padj) & df$padj <= p,])}

pVals <- c(0.001, 0.01, 0.05, 0.1)
fcVals <- c(0:(max(DE$log2FoldChange, na.rm= TRUE)+1))

summary <- do.call(rbind, lapply(pVals, function(p) {
  do.call(rbind, lapply(fcVals, function(f){
    up <- filterUP(DE, f, p)
    down <- filterDOWN(DE, f, p)
    return(data.frame("log2FoldChange" = f, "padj" = p, 
                      "upRegulated" = up, "downRegulated" = down))
  }))
}))

mdata <- melt(summary, id.vars = c('log2FoldChange', 'padj'))

p <- ggplot(mdata, aes(x = log2FoldChange, y = value)) + geom_bar(aes(fill = variable), stat = 'identity', position = 'dodge') + facet_grid(~ padj) + theme(legend.position = 'bottom', legend.title = element_blank()) + labs(title = 'Number of differentially up/down regulated genes', subtitle = 'based on different p-value and log2foldChange cut-off values')
print(p)

#save image to folder
pdf(file = file.path(imagesDir, 'up_down_regulated_genes_summary.pdf'))
print(p)
invisible(dev.off())

# interactive boxplot
select <- rownames(DEsubset)
if(length(select) > 1) {
  expressionLevels <- reshape2::melt(vsd.counts[select,])
  colnames(expressionLevels) <- c('geneId', 'sampleName', 'expressionLevel')
  
  expressionLevels$celltype <- coldata[expressionLevels$sampleName,]$celltype
  expressionLevels$treatment <- coldata[expressionLevels$sampleName,]$treatment
  
  matchIds <- match(expressionLevels$geneId, rownames(DE))
  expressionLevels$padj <- DE[matchIds,]$padj
  expressionLevels$log2FoldChange <- DE[matchIds,]$log2FoldChange
  
  sd <- SharedData$new(expressionLevels, ~geneId)
  
  lineplot <- plot_ly(sd, x = ~sampleName, y = ~expressionLevel) %>%
    group_by(geneId) %>% 
    add_lines(text = ~geneId, hoverinfo = "text", color = ~treatment)
  
  volcanoPlot <- plot_ly(sd, x = ~log2FoldChange, y = ~-log10(padj)) %>% 
    add_markers(text = ~geneId, hoverinfo = "text")
  
  subplot(
    plot_ly(sd, y = ~expressionLevel, color = ~treatment) %>% 
      add_boxplot(),  
    volcanoPlot
  ) %>% highlight(on =  'plotly_click', off = 'plotly_doubleclick', selectize = TRUE)
} else {
  cat("Couldn't detect at least two genes satisfying the p-value and fold change thresholds\n")
}