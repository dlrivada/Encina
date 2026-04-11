```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 18,777.1 ns | 132.54 ns | 190.09 ns | 34.80 |    0.43 |    2 | 0.2136 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 42,296.8 ns | 202.23 ns | 302.68 ns | 78.39 |    0.81 |    3 | 1.6479 | 0.0610 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    539.6 ns |   2.82 ns |   4.13 ns |  1.00 |    0.01 |    1 | 0.0315 |      - |     800 B |        1.00 |
