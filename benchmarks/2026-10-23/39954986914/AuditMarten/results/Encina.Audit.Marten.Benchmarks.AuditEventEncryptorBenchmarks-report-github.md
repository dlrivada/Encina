```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 19,439.5 ns | 1,674.41 ns | 91.78 ns | 30.69 |    0.17 |    2 | 0.2136 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 41,644.5 ns | 1,208.05 ns | 66.22 ns | 65.74 |    0.26 |    3 | 1.6479 | 0.0610 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    633.5 ns |    48.48 ns |  2.66 ns |  1.00 |    0.01 |    1 | 0.0315 |      - |     800 B |        1.00 |
