```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                        | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------ |---------:|------:|------:|-----:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  | 92.23 ms |    NA |  1.00 |    4 |  12.35 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 93.47 ms |    NA |  1.01 |    5 |  55.96 KB |        4.53 |
| &#39;JSON export: 200 activities&#39; | 94.10 ms |    NA |  1.02 |    6 | 220.13 KB |       17.82 |
| &#39;CSV export: 10 activities&#39;   | 27.65 ms |    NA |  0.30 |    2 |  55.83 KB |        4.52 |
| &#39;CSV export: 50 activities&#39;   | 27.43 ms |    NA |  0.30 |    1 |  233.9 KB |       18.94 |
| &#39;CSV export: 200 activities&#39;  | 28.61 ms |    NA |  0.31 |    3 |  913.3 KB |       73.94 |
