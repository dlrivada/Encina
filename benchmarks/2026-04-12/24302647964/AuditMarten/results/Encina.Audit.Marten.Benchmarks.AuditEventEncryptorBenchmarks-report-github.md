```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 19,055.5 ns |  37.46 ns |  53.72 ns | 36.36 |    0.24 |    2 | 0.2136 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 39,825.9 ns | 543.05 ns | 812.81 ns | 76.00 |    1.59 |    3 | 1.6479 | 0.0610 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    524.0 ns |   2.22 ns |   3.18 ns |  1.00 |    0.01 |    1 | 0.0315 |      - |     800 B |        1.00 |
