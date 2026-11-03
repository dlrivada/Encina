```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.39GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.636 ns |  0.5667 ns | 0.0311 ns |   0.40 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.007 ns |  2.4761 ns | 0.1357 ns |   1.00 |    0.02 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   866.163 ns | 55.3844 ns | 3.0358 ns |  96.18 |    1.30 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,327.047 ns | 81.4084 ns | 4.4623 ns | 147.36 |    1.99 | 0.0191 |     504 B |       12.60 |
