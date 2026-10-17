```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.972 ns |  0.0117 ns | 0.0006 ns |   0.43 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.317 ns |  2.6867 ns | 0.1473 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   794.563 ns | 61.3506 ns | 3.3628 ns |  85.30 |    1.20 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,168.852 ns | 31.6362 ns | 1.7341 ns | 125.48 |    1.72 | 0.0286 |     504 B |       12.60 |
