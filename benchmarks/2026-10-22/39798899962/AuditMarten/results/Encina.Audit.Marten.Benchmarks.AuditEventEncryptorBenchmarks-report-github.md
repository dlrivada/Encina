```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 19,261.9 ns |  3,443.70 ns | 188.76 ns | 35.39 |    0.32 |    2 | 0.3052 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 39,653.4 ns | 11,060.86 ns | 606.28 ns | 72.86 |    1.00 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    544.3 ns |     38.37 ns |   2.10 ns |  1.00 |    0.00 |    1 | 0.0477 |      - |     800 B |        1.00 |
