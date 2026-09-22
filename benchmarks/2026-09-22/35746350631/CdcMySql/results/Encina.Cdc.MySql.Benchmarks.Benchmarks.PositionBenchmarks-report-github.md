```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.666 ns |   1.338 ns |  0.0733 ns |   0.35 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.719 ns |   2.512 ns |  0.1377 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 456.586 ns | 321.927 ns | 17.6459 ns |  96.81 |    4.05 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 666.011 ns |  57.269 ns |  3.1391 ns | 141.22 |    3.58 | 0.0296 |     504 B |       12.60 |
