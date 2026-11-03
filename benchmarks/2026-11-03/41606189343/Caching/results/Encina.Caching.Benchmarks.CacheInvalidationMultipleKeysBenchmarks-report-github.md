```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **27.09 μs** |      **4.577 μs** |     **0.251 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |  **17.13 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,186.26 μs** | **10,974.908 μs** |   **601.572 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **7,008.73 μs** | **32,593.312 μs** | **1,786.549 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.17 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **9,900.15 μs** | **27,130.183 μs** | **1,487.097 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.12 KB** |           **?** |
