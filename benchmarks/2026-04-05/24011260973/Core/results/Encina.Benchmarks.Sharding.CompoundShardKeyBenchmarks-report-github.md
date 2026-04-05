```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                          | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Extract: IShardable (simple)&#39;                  | 21.75 ms |    NA |  1.00 |    7 |      24 B |        1.00 |
| &#39;Extract: ICompoundShardable (2 components)&#39;    | 20.87 ms |    NA |  0.96 |    6 |     432 B |       18.00 |
| &#39;Extract: ICompoundShardable (3 components)&#39;    | 20.77 ms |    NA |  0.95 |    5 |     464 B |       19.33 |
| &#39;Extract: ICompoundShardable (5 components)&#39;    | 20.48 ms |    NA |  0.94 |    4 |     528 B |       22.00 |
| &#39;Extract: [ShardKey] attributes (2 components)&#39; | 26.90 ms |    NA |  1.24 |    8 |    3040 B |      126.67 |
| &#39;Route: HashRouter (simple string key)&#39;         | 17.58 ms |    NA |  0.81 |    1 |      64 B |        2.67 |
| &#39;Route: HashRouter (compound key)&#39;              | 18.07 ms |    NA |  0.83 |    2 |     240 B |       10.00 |
| &#39;Route: CompoundRouter (2 components)&#39;          | 20.07 ms |    NA |  0.92 |    3 |     296 B |       12.33 |
