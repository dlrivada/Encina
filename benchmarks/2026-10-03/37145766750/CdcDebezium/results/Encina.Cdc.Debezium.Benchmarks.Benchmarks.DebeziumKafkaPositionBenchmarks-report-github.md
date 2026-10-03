```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|------------:|----------:|-------:|----------:|
| ComparePositions |     3.473 ns |   0.1741 ns | 0.0095 ns |      - |         - |
| FromBytes        | 2,019.713 ns | 162.4056 ns | 8.9020 ns | 0.0420 |     744 B |
| ToBytes          |   929.605 ns |  12.5076 ns | 0.6856 ns | 0.0153 |     264 B |
