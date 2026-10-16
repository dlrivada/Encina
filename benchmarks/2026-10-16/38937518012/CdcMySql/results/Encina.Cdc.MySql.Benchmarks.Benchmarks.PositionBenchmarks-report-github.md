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
| CompareFilePositions |     3.982 ns |  0.0422 ns | 0.0023 ns |   0.47 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.430 ns |  0.7666 ns | 0.0420 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   763.296 ns | 66.7009 ns | 3.6561 ns |  90.54 |    0.54 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,158.666 ns | 42.3085 ns | 2.3191 ns | 137.44 |    0.64 | 0.0286 |     504 B |       12.60 |
