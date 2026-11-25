```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 16,365.7 ns | 4,079.74 ns | 223.62 ns | 26.13 |    0.37 |    2 | 0.2136 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 35,566.0 ns | 4,058.48 ns | 222.46 ns | 56.78 |    0.52 |    3 | 1.6479 | 0.0610 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    626.4 ns |    98.60 ns |   5.40 ns |  1.00 |    0.01 |    1 | 0.0315 |      - |     800 B |        1.00 |
