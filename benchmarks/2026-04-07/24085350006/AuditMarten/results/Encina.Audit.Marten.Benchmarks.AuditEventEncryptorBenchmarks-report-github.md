```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 17,517.5 ns | 1,025.48 ns |  56.21 ns | 33.50 |    0.14 |    2 | 0.3052 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 38,637.0 ns | 1,864.70 ns | 102.21 ns | 73.89 |    0.28 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    522.9 ns |    33.54 ns |   1.84 ns |  1.00 |    0.00 |    1 | 0.0477 |      - |     800 B |        1.00 |
