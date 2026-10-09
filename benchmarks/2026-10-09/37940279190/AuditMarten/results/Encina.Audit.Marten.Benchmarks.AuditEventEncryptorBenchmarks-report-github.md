```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 18,144.2 ns |    38.82 ns |  2.13 ns | 33.87 |    0.10 |    2 | 0.3052 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 40,814.6 ns | 1,418.60 ns | 77.76 ns | 76.18 |    0.26 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    535.8 ns |    33.95 ns |  1.86 ns |  1.00 |    0.00 |    1 | 0.0477 |      - |     800 B |        1.00 |
