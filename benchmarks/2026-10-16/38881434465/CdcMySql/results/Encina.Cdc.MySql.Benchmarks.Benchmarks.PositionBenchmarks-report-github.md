```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.082 ns |   0.3621 ns |  0.0198 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.597 ns |   3.5544 ns |  0.1948 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   785.640 ns | 182.8036 ns | 10.0201 ns |  91.42 |    2.07 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,208.598 ns | 379.2786 ns | 20.7895 ns | 140.64 |    3.48 | 0.0286 |     504 B |       12.60 |
