```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 16,424.5 ns |   403.87 ns |  22.14 ns | 30.16 |    0.13 |    2 | 0.3052 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 35,733.6 ns | 4,347.09 ns | 238.28 ns | 65.61 |    0.46 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    544.6 ns |    46.00 ns |   2.52 ns |  1.00 |    0.01 |    1 | 0.0477 |      - |     800 B |        1.00 |
