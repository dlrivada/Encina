```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.988 ns |  0.5731 ns | 0.0314 ns |   0.42 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.512 ns |  0.6945 ns | 0.0381 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   803.615 ns | 35.2072 ns | 1.9298 ns |  84.48 |    0.34 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,223.318 ns | 53.4113 ns | 2.9277 ns | 128.60 |    0.52 | 0.0286 |     504 B |       12.60 |
