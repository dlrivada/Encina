```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.703 ns |   0.6732 ns | 0.0369 ns |   0.36 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.541 ns |   1.0410 ns | 0.0571 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 547.260 ns |  86.3619 ns | 4.7338 ns |  72.58 |    0.72 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 845.853 ns | 118.9154 ns | 6.5182 ns | 112.18 |    1.05 | 0.0296 |     504 B |       12.60 |
