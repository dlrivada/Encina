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
| CompareFilePositions |     3.983 ns |  0.2018 ns | 0.0111 ns |   0.43 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.161 ns |  3.1485 ns | 0.1726 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   779.241 ns | 39.8346 ns | 2.1835 ns |  85.08 |    1.40 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,171.600 ns | 98.6310 ns | 5.4063 ns | 127.93 |    2.14 | 0.0286 |     504 B |       12.60 |
