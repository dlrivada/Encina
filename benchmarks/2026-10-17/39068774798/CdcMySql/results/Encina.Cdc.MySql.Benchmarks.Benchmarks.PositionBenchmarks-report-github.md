```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.692 ns |  0.5125 ns | 0.0281 ns |   0.38 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.026 ns |  4.6025 ns | 0.2523 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 546.653 ns | 59.1363 ns | 3.2415 ns |  77.87 |    2.45 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 862.667 ns | 50.8431 ns | 2.7869 ns | 122.88 |    3.83 | 0.0296 |     504 B |       12.60 |
