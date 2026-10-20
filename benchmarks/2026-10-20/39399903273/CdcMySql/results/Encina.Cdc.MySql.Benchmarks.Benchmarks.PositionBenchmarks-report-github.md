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
| CompareFilePositions |     3.979 ns |   0.1401 ns |  0.0077 ns |   0.43 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.209 ns |   2.0422 ns |  0.1119 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   796.494 ns | 127.4901 ns |  6.9882 ns |  86.49 |    1.12 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,175.563 ns | 326.3589 ns | 17.8888 ns | 127.66 |    2.16 | 0.0286 |     504 B |       12.60 |
