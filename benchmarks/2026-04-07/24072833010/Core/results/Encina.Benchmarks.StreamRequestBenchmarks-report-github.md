```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                  | Mean     | Error | Ratio | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|------:|----------:|------------:|
| Stream_SmallDataset_10Items             | 68.41 ms |    NA |  1.00 |   17320 B |        1.00 |
| Stream_MediumDataset_100Items           | 67.45 ms |    NA |  0.99 |   35248 B |        2.04 |
| Stream_LargeDataset_1000Items           | 68.24 ms |    NA |  1.00 |  230392 B |       13.30 |
| Stream_WithPipelineBehaviors            | 79.90 ms |    NA |  1.17 |   41616 B |        2.40 |
| Stream_MaterializeToList_100Items       | 69.23 ms |    NA |  1.01 |   46192 B |        2.67 |
| Stream_CountOnly_NoMaterialization      | 68.67 ms |    NA |  1.00 |   35568 B |        2.05 |
| Stream_WithCancellation_EarlyExit       | 69.90 ms |    NA |  1.02 |   53200 B |        3.07 |
| Stream_DirectHandlerInvocation_NoEncina | 14.61 ms |    NA |  0.21 |     400 B |        0.02 |
