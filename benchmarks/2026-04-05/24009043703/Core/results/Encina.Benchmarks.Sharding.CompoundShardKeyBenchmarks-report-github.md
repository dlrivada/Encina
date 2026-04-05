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
| &#39;Extract: IShardable (simple)&#39;                  | 21.71 ms |    NA |  1.00 |    7 |      24 B |        1.00 |
| &#39;Extract: ICompoundShardable (2 components)&#39;    | 20.97 ms |    NA |  0.97 |    5 |     432 B |       18.00 |
| &#39;Extract: ICompoundShardable (3 components)&#39;    | 20.85 ms |    NA |  0.96 |    4 |     464 B |       19.33 |
| &#39;Extract: ICompoundShardable (5 components)&#39;    | 21.02 ms |    NA |  0.97 |    6 |     528 B |       22.00 |
| &#39;Extract: [ShardKey] attributes (2 components)&#39; | 28.26 ms |    NA |  1.30 |    8 |    3040 B |      126.67 |
| &#39;Route: HashRouter (simple string key)&#39;         | 17.96 ms |    NA |  0.83 |    1 |      64 B |        2.67 |
| &#39;Route: HashRouter (compound key)&#39;              | 18.47 ms |    NA |  0.85 |    2 |     240 B |       10.00 |
| &#39;Route: CompoundRouter (2 components)&#39;          | 20.19 ms |    NA |  0.93 |    3 |     296 B |       12.33 |
