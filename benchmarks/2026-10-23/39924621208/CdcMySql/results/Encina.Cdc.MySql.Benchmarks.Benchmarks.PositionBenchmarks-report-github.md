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
| CompareFilePositions |     3.646 ns |  0.0280 ns | 0.0015 ns |   0.37 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.887 ns |  0.7151 ns | 0.0392 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   779.019 ns | 26.0344 ns | 1.4270 ns |  78.79 |    0.30 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,194.362 ns | 50.5060 ns | 2.7684 ns | 120.80 |    0.48 | 0.0286 |     504 B |       12.60 |
