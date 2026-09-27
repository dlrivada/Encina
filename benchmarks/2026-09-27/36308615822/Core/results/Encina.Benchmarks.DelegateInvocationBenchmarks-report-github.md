```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.93 ns |      1.832 ns |   0.100 ns |     1.28 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.97 ns |      1.423 ns |   0.078 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 63,676.67 ns | 18,001.381 ns | 986.716 ns | 2,276.26 |   31.04 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.29 ns |     11.487 ns |   0.630 ns |     4.76 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.71 ns |      6.402 ns |   0.351 ns |     3.03 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
