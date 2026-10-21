```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.779 ns |  2.758 ns | 0.1512 ns |   0.39 |    0.03 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.543 ns |  1.494 ns | 0.0819 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 410.381 ns | 95.810 ns | 5.2516 ns |  90.36 |    1.73 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 614.722 ns | 19.161 ns | 1.0503 ns | 135.35 |    2.12 | 0.0296 |     504 B |       12.60 |
