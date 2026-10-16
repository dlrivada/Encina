```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.016 ns |  1.3633 ns | 0.0747 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.418 ns |  0.5000 ns | 0.0274 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   784.742 ns | 54.6860 ns | 2.9975 ns |  93.22 |    0.40 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,164.799 ns | 89.8928 ns | 4.9273 ns | 138.37 |    0.64 | 0.0286 |     504 B |       12.60 |
