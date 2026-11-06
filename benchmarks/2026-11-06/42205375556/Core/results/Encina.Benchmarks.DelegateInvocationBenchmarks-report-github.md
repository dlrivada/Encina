```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.95 ns |      0.462 ns |   0.025 ns |     1.25 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.50 ns |      0.649 ns |   0.036 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,061.87 ns | 15,191.982 ns | 832.724 ns | 2,468.24 |   33.73 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |     98.46 ns |      8.781 ns |   0.481 ns |     4.58 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     67.56 ns |     13.737 ns |   0.753 ns |     3.14 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
