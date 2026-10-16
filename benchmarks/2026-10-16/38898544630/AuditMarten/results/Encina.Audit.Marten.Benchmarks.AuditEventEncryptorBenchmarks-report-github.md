```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 18,434.2 ns |   705.05 ns | 38.65 ns | 32.37 |    0.14 |    2 | 0.3052 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 39,618.6 ns | 1,416.06 ns | 77.62 ns | 69.58 |    0.30 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    569.4 ns |    47.73 ns |  2.62 ns |  1.00 |    0.01 |    1 | 0.0477 |      - |     800 B |        1.00 |
