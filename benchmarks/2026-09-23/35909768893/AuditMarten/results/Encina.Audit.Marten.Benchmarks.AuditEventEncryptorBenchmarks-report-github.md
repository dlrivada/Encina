```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 13,952.7 ns | 1,013.29 ns | 55.54 ns | 34.04 |    0.17 |    2 | 0.3204 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 30,038.5 ns |   478.61 ns | 26.23 ns | 73.29 |    0.28 |    3 | 2.4719 | 0.0916 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    409.8 ns |    31.90 ns |  1.75 ns |  1.00 |    0.01 |    1 | 0.0477 |      - |     800 B |        1.00 |
