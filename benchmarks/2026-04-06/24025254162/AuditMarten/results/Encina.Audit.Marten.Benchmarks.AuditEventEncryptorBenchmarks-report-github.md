```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptAuditEntry_Minimal_NoPii |    556.0 ns |    56.71 ns |   3.11 ns |  1.00 |    0.01 |    1 | 0.0477 |      - |     800 B |        1.00 |
| EncryptAuditEntry_Full_AllPii   | 41,290.5 ns | 7,940.55 ns | 435.25 ns | 74.27 |    0.77 |    3 | 2.4414 | 0.0610 |   41713 B |       52.14 |
| EncryptReadAuditEntry           | 18,910.0 ns | 1,745.43 ns |  95.67 ns | 34.01 |    0.22 |    2 | 0.3052 |      - |    5600 B |        7.00 |
