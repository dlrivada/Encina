```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.83 ns |     5.965 ns |   0.327 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.09 ns |     4.462 ns |   0.245 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,491.07 ns | 2,544.287 ns | 139.461 ns | 2,422.15 |   23.81 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    110.15 ns |    27.812 ns |   1.524 ns |     4.99 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     68.50 ns |    10.963 ns |   0.601 ns |     3.10 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
