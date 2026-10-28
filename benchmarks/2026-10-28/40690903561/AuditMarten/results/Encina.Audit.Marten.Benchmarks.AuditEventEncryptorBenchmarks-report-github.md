```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 13,497.6 ns | 4,562.7 ns | 250.09 ns | 29.87 |    0.85 |    2 | 0.3204 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 31,916.1 ns | 3,886.5 ns | 213.03 ns | 70.62 |    1.70 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    452.1 ns |   225.0 ns |  12.33 ns |  1.00 |    0.03 |    1 | 0.0477 |      - |     800 B |        1.00 |
