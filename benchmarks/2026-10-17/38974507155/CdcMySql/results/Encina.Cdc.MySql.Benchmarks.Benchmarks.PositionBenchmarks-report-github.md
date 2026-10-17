```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.831 ns |  0.3465 ns | 0.0190 ns |  0.29 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   9.764 ns |  7.9427 ns | 0.4354 ns |  1.00 |    0.06 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 537.766 ns | 21.4958 ns | 1.1783 ns | 55.15 |    2.17 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 731.916 ns | 43.4591 ns | 2.3821 ns | 75.06 |    2.96 | 0.0057 |     504 B |       12.60 |
