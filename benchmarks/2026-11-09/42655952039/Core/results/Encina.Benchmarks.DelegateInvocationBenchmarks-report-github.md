```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.85 ns |     0.846 ns |   0.046 ns |     1.25 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.40 ns |     0.868 ns |   0.048 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,109.10 ns | 7,887.447 ns | 432.338 ns | 2,481.56 |   18.13 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    101.16 ns |    12.791 ns |   0.701 ns |     4.73 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.03 ns |     3.453 ns |   0.189 ns |     3.09 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
