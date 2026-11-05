```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.19 ns |     7.888 ns |   0.432 ns |     1.31 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.44 ns |     3.197 ns |   0.175 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,029.40 ns | 6,452.132 ns | 353.663 ns | 3,165.63 |   20.06 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.29 ns |    23.403 ns |   1.283 ns |     4.69 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.18 ns |    23.471 ns |   1.287 ns |     2.96 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
