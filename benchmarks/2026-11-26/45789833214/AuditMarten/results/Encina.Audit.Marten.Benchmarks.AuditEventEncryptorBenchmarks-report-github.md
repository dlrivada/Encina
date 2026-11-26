```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.41GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 16,399.1 ns | 2,073.10 ns | 113.63 ns | 26.60 |    0.20 |    2 | 0.2136 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 34,989.8 ns | 2,251.43 ns | 123.41 ns | 56.76 |    0.32 |    3 | 1.6479 | 0.0610 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    616.4 ns |    60.97 ns |   3.34 ns |  1.00 |    0.01 |    1 | 0.0315 |      - |     800 B |        1.00 |
