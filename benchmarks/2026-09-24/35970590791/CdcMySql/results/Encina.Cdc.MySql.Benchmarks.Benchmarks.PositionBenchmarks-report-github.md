```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.990 ns |  0.5652 ns | 0.0310 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.454 ns |  2.3448 ns | 0.1285 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   783.753 ns | 14.5550 ns | 0.7978 ns |  92.72 |    1.21 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,131.705 ns | 41.2964 ns | 2.2636 ns | 133.89 |    1.76 | 0.0286 |     504 B |       12.60 |
