```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.43 ns |     6.735 ns |   0.369 ns |     1.29 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.95 ns |     3.115 ns |   0.171 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,263.97 ns | 3,338.362 ns | 182.987 ns | 3,187.23 |   17.13 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.50 ns |    37.803 ns |   2.072 ns |     4.51 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     79.20 ns |     6.414 ns |   0.352 ns |     2.74 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
