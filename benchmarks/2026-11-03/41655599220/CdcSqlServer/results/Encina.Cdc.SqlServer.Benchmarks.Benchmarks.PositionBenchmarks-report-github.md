```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.4517 ns | 0.4449 ns | 0.0244 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 3.3006 ns | 0.3531 ns | 0.0194 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.5256 ns | 0.0763 ns | 0.0042 ns |  1.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.1952 ns | 0.2042 ns | 0.0112 ns |  0.97 | 0.0019 |      32 B |        1.33 |
