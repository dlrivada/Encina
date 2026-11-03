```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.562 ns |  0.3952 ns | 0.0217 ns |   0.40 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.933 ns |  0.9225 ns | 0.0506 ns |   1.00 |    0.01 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   860.337 ns | 21.9384 ns | 1.2025 ns |  96.31 |    0.49 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,305.655 ns | 92.0505 ns | 5.0456 ns | 146.17 |    0.87 | 0.0191 |     504 B |       12.60 |
