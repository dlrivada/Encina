```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.79 ns |     16.506 ns |   0.905 ns |     1.27 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.15 ns |      1.244 ns |   0.068 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 66,689.09 ns | 10,001.900 ns | 548.238 ns | 2,368.77 |   17.58 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.21 ns |      4.568 ns |   0.250 ns |     4.87 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     87.41 ns |     13.517 ns |   0.741 ns |     3.10 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
