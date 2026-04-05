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
| &#39;JSON export: 10 activities&#39;  | 91.97 ms |    NA |  1.00 |    4 |  12.33 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 92.77 ms |    NA |  1.01 |    5 |  55.96 KB |        4.54 |
| &#39;JSON export: 200 activities&#39; | 92.80 ms |    NA |  1.01 |    6 | 220.13 KB |       17.86 |
| &#39;CSV export: 10 activities&#39;   | 27.31 ms |    NA |  0.30 |    1 |  55.83 KB |        4.53 |
| &#39;CSV export: 50 activities&#39;   | 27.59 ms |    NA |  0.30 |    2 |  233.9 KB |       18.97 |
| &#39;CSV export: 200 activities&#39;  | 27.90 ms |    NA |  0.30 |    3 |  913.3 KB |       74.08 |
