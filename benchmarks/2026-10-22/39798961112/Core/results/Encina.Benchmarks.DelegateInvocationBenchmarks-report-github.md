```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.26 ns |   0.107 ns |   0.161 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.75 ns |   0.056 ns |   0.082 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 51,460.55 ns | 204.183 ns | 292.833 ns | 2,366.52 |   15.88 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    103.09 ns |   0.311 ns |   0.445 ns |     4.74 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.89 ns |   0.688 ns |   1.030 ns |     3.03 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
