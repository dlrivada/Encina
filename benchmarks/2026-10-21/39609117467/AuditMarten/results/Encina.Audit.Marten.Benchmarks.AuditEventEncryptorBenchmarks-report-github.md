```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 13,106.0 ns |   727.22 ns | 39.86 ns | 30.06 |    0.11 |    2 | 0.3204 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 27,954.3 ns | 1,258.07 ns | 68.96 ns | 64.13 |    0.21 |    3 | 2.4719 | 0.0916 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    435.9 ns |    22.52 ns |  1.23 ns |  1.00 |    0.00 |    1 | 0.0477 |      - |     800 B |        1.00 |
