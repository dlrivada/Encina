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
| CompareFilePositions |     3.993 ns |  0.7231 ns | 0.0396 ns |   0.46 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.713 ns |  0.8161 ns | 0.0447 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   792.022 ns | 36.3779 ns | 1.9940 ns |  90.90 |    0.45 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,171.980 ns |  7.7369 ns | 0.4241 ns | 134.51 |    0.60 | 0.0286 |     504 B |       12.60 |
