```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error      | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|-----------:|----------:|-------:|----------:|
| ComparePositions |     2.711 ns |  0.7600 ns | 0.0417 ns |      - |         - |
| FromBytes        | 1,485.173 ns | 43.4903 ns | 2.3839 ns | 0.0439 |     744 B |
| ToBytes          |   685.039 ns | 17.3909 ns | 0.9533 ns | 0.0153 |     264 B |
