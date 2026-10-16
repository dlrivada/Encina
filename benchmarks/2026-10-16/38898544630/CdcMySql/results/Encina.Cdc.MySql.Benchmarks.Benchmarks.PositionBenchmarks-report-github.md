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
| CompareFilePositions |     3.970 ns |  0.0472 ns | 0.0026 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.303 ns |  1.9495 ns | 0.1069 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   778.739 ns | 91.1013 ns | 4.9936 ns |  93.80 |    1.17 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,171.412 ns | 66.0919 ns | 3.6227 ns | 141.10 |    1.62 | 0.0286 |     504 B |       12.60 |
