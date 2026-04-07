```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                        | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------ |---------:|------:|------:|-----:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  | 89.70 ms |    NA |  1.00 |    5 |  12.35 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 89.88 ms |    NA |  1.00 |    6 |  56.06 KB |        4.54 |
| &#39;JSON export: 200 activities&#39; | 88.32 ms |    NA |  0.98 |    4 | 220.13 KB |       17.82 |
| &#39;CSV export: 10 activities&#39;   | 25.85 ms |    NA |  0.29 |    2 |  55.83 KB |        4.52 |
| &#39;CSV export: 50 activities&#39;   | 25.75 ms |    NA |  0.29 |    1 |  233.9 KB |       18.94 |
| &#39;CSV export: 200 activities&#39;  | 26.02 ms |    NA |  0.29 |    3 |  913.3 KB |       73.94 |
